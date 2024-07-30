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

/* Scripts/Items/Special/Holiday/Halloween/SarcophagusAddon.cs
 * CHANGELOG
 *  3/16/2024, Adam (Dupe() override)
 *      When duping certain objects that have a non-zero constructor necessary to replicate the object in question, a Dupe() override is needed.
 *  10/5/23, Yoar
 *      Initial commit.
 */

namespace Server.Items
{
    public class SarcophagusAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new SarcophagusAddonDeed(m_Lady); } }
        public override bool Redeedable { get { return true; } }

        private bool m_Lady;
        private bool m_East;
        private bool m_Opened;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Opened
        {
            get { return m_Opened; }
            set
            {
                if (m_Opened != value)
                {
                    if (value)
                        Open();
                    else
                        Close();
                }
            }
        }

        [Constructable]
        public SarcophagusAddon()
            : this(1)
        {
        }

        [Constructable]
        public SarcophagusAddon(int type)
        {
            m_Lady = ((type / 2) == 1);
            m_East = ((type % 2) == 0);

            if (m_Lady)
            {
                if (m_East)
                {
                    AddComponent(new AddonComponent(0x1C6C), -1, 0, 0);
                    AddComponent(new AddonComponent(0x1C6D), 0, 0, 0);
                    AddComponent(new AddonComponent(0x1C6E), 1, 0, 0);
                    AddComponent(new AddonComponent(0x1C6B), -1, 1, 0);
                    AddComponent(new AddonComponent(0x1C6A), 0, 1, 0);
                    AddComponent(new AddonComponent(0x1C69), 1, 1, 0);

                    AddComponent(new AddonComponent(0x1C66), -1, 1, 5);
                    AddComponent(new AddonComponent(0x1C67), 0, 1, 5);
                    AddComponent(new AddonComponent(0x1C68), 1, 1, 5);
                }
                else
                {
                    AddComponent(new AddonComponent(0x1C9C), 0, -1, 0);
                    AddComponent(new AddonComponent(0x1C9D), 0, 0, 0);
                    AddComponent(new AddonComponent(0x1C9E), 0, 1, 0);
                    AddComponent(new AddonComponent(0x1C9B), 1, -1, 0);
                    AddComponent(new AddonComponent(0x1C9A), 1, 0, 0);
                    AddComponent(new AddonComponent(0x1C99), 1, 1, 0);

                    AddComponent(new AddonComponent(0x1C96), 1, -1, 5);
                    AddComponent(new AddonComponent(0x1C97), 1, 0, 5);
                    AddComponent(new AddonComponent(0x1C98), 1, 1, 5);
                }
            }
            else
            {
                if (m_East)
                {
                    AddComponent(new AddonComponent(0x1C83), -1, 0, 0);
                    AddComponent(new AddonComponent(0x1C84), 0, 0, 0);
                    AddComponent(new AddonComponent(0x1C85), 1, 0, 0);
                    AddComponent(new AddonComponent(0x1C82), -1, 1, 0);
                    AddComponent(new AddonComponent(0x1C81), 0, 1, 0);
                    AddComponent(new AddonComponent(0x1C80), 1, 1, 0);

                    AddComponent(new AddonComponent(0x1C7D), -1, 1, 5);
                    AddComponent(new AddonComponent(0x1C7E), 0, 1, 5);
                    AddComponent(new AddonComponent(0x1C7F), 1, 1, 5);
                }
                else
                {
                    AddComponent(new AddonComponent(0x1CB3), 0, -1, 0);
                    AddComponent(new AddonComponent(0x1CB4), 0, 0, 0);
                    AddComponent(new AddonComponent(0x1CB5), 0, 1, 0);
                    AddComponent(new AddonComponent(0x1CB2), 1, -1, 0);
#if false
                    AddComponent( new AddonComponent( 0x1CB1 ), 1, 0, 0 );
#else
                    AddComponent(new AddonComponent(0x1D82), 1, 0, 0);
#endif
                    AddComponent(new AddonComponent(0x1CB0), 1, 1, 0);

                    AddComponent(new AddonComponent(0x1CAD), 1, -1, 5);
                    AddComponent(new AddonComponent(0x1CAE), 1, 0, 5);
                    AddComponent(new AddonComponent(0x1CAF), 1, 1, 5);
                }
            }
        }
        public override Item Dupe(int amount)
        {
            // public override BaseAddon Addon { get { return new SarcophagusAddon(m_Type + (m_Lady ? 2 : 0)); } }
            int type = m_East ? 0 : 1;
            SarcophagusAddon new_addon = new SarcophagusAddon(type + (m_Lady ? 2 : 0));
            return base.Dupe(new_addon, amount);
        }
        public override void OnComponentDoubleClick(AddonComponent c, Mobile from)
        {
            if (m_Opened)
                Close();
            else if (CanOpen())
                Open();
        }

        private bool CanOpen()
        {
            if (Map == null)
                return true;

            Point3D offset = (m_East ? new Point3D(2, 1, 0) : new Point3D(1, 2, 0));

            return Map.CanFit(X + offset.X, Y + offset.Y, Z + offset.Z, 16, false, true, false);
        }

        private void Open()
        {
            m_Opened = true;

            int type = Randomize();

            SetType(type);

            Point3D offset = (m_East ? new Point3D(1, 0, 0) : new Point3D(0, 1, 0));

            for (int i = 6; i < 9; i++)
                MoveComponent(i, offset);

            Effects.PlaySound(Location, Map, 0xED);

            if (type != 0)
                Effects.PlaySound(Location, Map, Utility.RandomBool() ? 0x3EA : 0x3EC);
        }

        private void Close()
        {
            m_Opened = false;

            SetType(0);

            Point3D offset = (m_East ? new Point3D(-1, 0, 0) : new Point3D(0, -1, 0));

            for (int i = 6; i < 9; i++)
                MoveComponent(i, offset);

            Effects.PlaySound(Location, Map, 0xF4);
        }

        private int Randomize()
        {
            switch (Utility.Random(10))
            {
                case 1: return 1;
                case 2: return 2;
            }

            return 0;
        }

        private void SetType(int type)
        {
            switch (type)
            {
                case 0: // empty
                    {
                        if (m_Lady)
                        {
                            if (m_East)
                            {
                                SetComponentID(0, 0x1C6C);
                                SetComponentID(1, 0x1C6D);
                                SetComponentID(2, 0x1C6E);
                                SetComponentID(3, 0x1C6B);
                                SetComponentID(4, 0x1C6A);
                                SetComponentID(5, 0x1C69);
                            }
                            else
                            {
                                SetComponentID(0, 0x1C9C);
                                SetComponentID(1, 0x1C9D);
                                SetComponentID(2, 0x1C9E);
                                SetComponentID(3, 0x1C9B);
                                SetComponentID(4, 0x1C9A);
                                SetComponentID(5, 0x1C99);
                            }
                        }
                        else
                        {
                            if (m_East)
                            {
                                SetComponentID(0, 0x1C83);
                                SetComponentID(1, 0x1C84);
                                SetComponentID(2, 0x1C85);
                                SetComponentID(3, 0x1C82);
                                SetComponentID(4, 0x1C81);
                                SetComponentID(5, 0x1C80);
                            }
                            else
                            {
                                SetComponentID(0, 0x1CB3);
                                SetComponentID(1, 0x1CB4);
                                SetComponentID(2, 0x1CB5);
                                SetComponentID(3, 0x1CB2);
#if false
                            SetComponentID( 4, 0x1CB1 );
#else
                                SetComponentID(4, 0x1D82);
#endif
                                SetComponentID(5, 0x1CB0);
                            }
                        }

                        break;
                    }
                case 1: // body
                    {
                        if (m_Lady)
                        {
                            if (m_East)
                            {
                                SetComponentID(0, 0x1D20);
                                SetComponentID(1, 0x1D21);
                                SetComponentID(2, 0x1D22);
                                SetComponentID(3, 0x1D1F);
                                SetComponentID(4, 0x1D1E);
                                SetComponentID(5, 0x1D1D);
                            }
                            else
                            {
                                SetComponentID(0, 0x1D3D);
                                SetComponentID(1, 0x1D3E);
                                SetComponentID(2, 0x1C9E);
                                SetComponentID(3, 0x1D3C);
                                SetComponentID(4, 0x1D3B);
                                SetComponentID(5, 0x1D3A);
                            }
                        }
                        else
                        {
                            if (m_East)
                            {
                                SetComponentID(0, 0x1D2B);
                                SetComponentID(1, 0x1D2C);
                                SetComponentID(2, 0x1C85);
                                SetComponentID(3, 0x1D2A);
                                SetComponentID(4, 0x1D29);
                                SetComponentID(5, 0x1D28);
                            }
                            else
                            {
                                SetComponentID(0, 0x1D47);
                                SetComponentID(1, 0x1D48);
                                SetComponentID(2, 0x1CB5);
                                SetComponentID(3, 0x1D46);
                                SetComponentID(4, 0x1D45);
                                SetComponentID(5, 0x1D44);
                            }
                        }

                        break;
                    }
                case 2: // skeleton
                    {
                        if (m_Lady)
                        {
                            if (m_East)
                            {
                                SetComponentID(0, 0x1D5C);
                                SetComponentID(1, 0x1D5D);
                                SetComponentID(2, 0x1D22);
                                SetComponentID(3, 0x1D5B);
                                SetComponentID(4, 0x1D5A);
                                SetComponentID(5, 0x1D59);
                            }
                            else
                            {
                                SetComponentID(0, 0x1D7A);
                                SetComponentID(1, 0x1D7B);
                                SetComponentID(2, 0x1C9E);
                                SetComponentID(3, 0x1D79);
                                SetComponentID(4, 0x1D78);
                                SetComponentID(5, 0x1D77);
                            }
                        }
                        else
                        {
                            if (m_East)
                            {
                                SetComponentID(0, 0x1D66);
                                SetComponentID(1, 0x1D67);
                                SetComponentID(2, 0x1C85);
                                SetComponentID(3, 0x1D65);
                                SetComponentID(4, 0x1D64);
                                SetComponentID(5, 0x1D63);
                            }
                            else
                            {
                                SetComponentID(0, 0x1D84);
                                SetComponentID(1, 0x1D85);
                                SetComponentID(2, 0x1CB5);
                                SetComponentID(3, 0x1D83);
                                SetComponentID(4, 0x1D82);
                                SetComponentID(5, 0x1D81);
                            }
                        }

                        break;
                    }
            }
        }

        private void MoveComponent(int index, Point3D offset)
        {
            AddonComponent c = GetComponent(index);

            if (c != null)
            {
                c.Addon = null;
                c.Location += offset;
                c.Addon = this;
            }
        }

        private void SetComponentID(int index, int itemID)
        {
            AddonComponent c = GetComponent(index);

            if (c != null)
                c.ItemID = itemID;
        }

        private AddonComponent GetComponent(int index)
        {
            if (index >= 0 && index < Components.Count)
                return Components[index] as AddonComponent;

            return null;
        }

        public SarcophagusAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((bool)m_Lady);
            writer.Write((bool)m_East);
            writer.Write((bool)m_Opened);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Lady = reader.ReadBool();
            m_East = reader.ReadBool();
            m_Opened = reader.ReadBool();
        }
    }

    public class SarcophagusAddonDeed : BaseChoiceAddonDeed
    {
        public override string DefaultName { get { return "a sarcophagus deed"; } }
        public override BaseAddon Addon { get { return new SarcophagusAddon(m_Type + (m_Lady ? 2 : 0)); } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Sarcophagus (East)",
                "Sarcophagus (South)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        private bool m_Lady;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Lady
        {
            get { return m_Lady; }
            set { m_Lady = value; }
        }

        [Constructable]
        public SarcophagusAddonDeed()
            : this(Utility.RandomBool())
        {
        }

        [Constructable]
        public SarcophagusAddonDeed(bool lady)
            : base()
        {
            m_Lady = lady;
        }

        public SarcophagusAddonDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((bool)m_Lady);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Lady = reader.ReadBool();
        }
    }
}