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

/* Scripts/Items/Special/Holiday/Halloween/CoffinAddon.cs
 * CHANGELOG
 *  10/5/23, Yoar
 *      Initial commit.
 */

namespace Server.Items
{
    public class CoffinAddon : BaseAddon
    {
        public new static void Configure()
        {
            for (int i = 0; i < m_LidItemIDs.Length; i++)
                TileData.ItemTable[m_LidItemIDs[i] & TileData.MaxItemValue].Flags |= TileFlag.Surface;
        }

        private static readonly int[] m_LidItemIDs = new int[]
            {
                0x1C58, 0x1C59, 0x1C5A, 0x1C5B,
                0x1C5C, 0x1C5D, 0x1C5E, 0x1C5F,
            };

        public override BaseAddonDeed Deed { get { return new CoffinAddonDeed(m_Ankh); } }
        public override bool Redeedable { get { return true; } }

        private bool m_Ankh;
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
        public CoffinAddon()
            : this(1)
        {
        }

        [Constructable]
        public CoffinAddon(int type)
        {
            m_Ankh = ((type / 2) == 1);
            m_East = ((type % 2) == 0);

            if (m_East)
            {
                AddComponent(new AddonComponent(0x1C47), -1, 0, 0);
                AddComponent(new AddonComponent(0x1C48), 0, 0, 0);
                AddComponent(new AddonComponent(0x1C49), 1, 0, 0);

                AddComponent(new AddonComponent(0x1C58), -1, 0, 2);
                AddComponent(new AddonComponent(m_Ankh ? 0x1C59 : 0x1C5A), 0, 0, 2);
                AddComponent(new AddonComponent(0x1C5B), 1, 0, 2);

                AddComponent(new AddonComponent(0x2198), -1, 0, 4);
                AddComponent(new AddonComponent(0x2198), 0, 0, 4);
                AddComponent(new AddonComponent(0x2198), 1, 0, 4);
            }
            else
            {
                AddComponent(new AddonComponent(0x1C57), 0, -1, 0);
                AddComponent(new AddonComponent(0x1C56), 0, 0, 0);
                AddComponent(new AddonComponent(0x1C55), 0, 1, 0);

                AddComponent(new AddonComponent(0x1C5C), 0, -1, 2);
                AddComponent(new AddonComponent(m_Ankh ? 0x1C5D : 0x1C5E), 0, 0, 2);
                AddComponent(new AddonComponent(0x1C5F), 0, 1, 2);

                AddComponent(new AddonComponent(0x2198), 0, -1, 4);
                AddComponent(new AddonComponent(0x2198), 0, 0, 4);
                AddComponent(new AddonComponent(0x2198), 0, 1, 4);
            }
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

            Point3D offset = (m_East ? new Point3D(2, 0, 0) : new Point3D(0, 2, 0));

            return (Map.CanFit(X, Y, Z + 4, 2, false, true, false) && Map.CanFit(X + offset.X, Y + offset.Y, Z + offset.Z, 16, false, true, false));
        }

        private void Open()
        {
            m_Opened = true;

            int type = Randomize();

            SetType(type);

            Point3D offset = (m_East ? new Point3D(1, 0, 0) : new Point3D(0, 1, 0));

            for (int i = 3; i < 9; i++)
                MoveComponent(i, offset);

            Effects.PlaySound(Location, Map, 0x2F);

            if (type != 0)
                Effects.PlaySound(Location, Map, Utility.RandomBool() ? 0x3EA : 0x3EC);
        }

        private void Close()
        {
            m_Opened = false;

            SetType(0);

            Point3D offset = (m_East ? new Point3D(-1, 0, 0) : new Point3D(0, -1, 0));

            for (int i = 3; i < 9; i++)
                MoveComponent(i, offset);

            Effects.PlaySound(Location, Map, 0x2E);
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
                        if (m_East)
                        {
                            SetComponentID(0, 0x1C47);
                            SetComponentID(1, 0x1C48);
                            SetComponentID(2, 0x1C49);
                        }
                        else
                        {
                            SetComponentID(0, 0x1C57);
                            SetComponentID(1, 0x1C56);
                            SetComponentID(2, 0x1C55);
                        }

                        break;
                    }
                case 1: // body
                    {
                        if (m_East)
                        {
                            SetComponentID(0, 0x1D1C);
                            SetComponentID(1, 0x1D1B);
                            SetComponentID(2, 0x1D1A);
                        }
                        else
                        {
                            SetComponentID(0, 0x1D39);
                            SetComponentID(1, 0x1D38);
                            SetComponentID(2, 0x1D37);
                        }

                        break;
                    }
                case 2: // skeleton
                    {
                        if (m_East)
                        {
                            SetComponentID(0, 0x1D54);
                            SetComponentID(1, 0x1D55);
                            SetComponentID(2, 0x1D56);
                        }
                        else
                        {
                            SetComponentID(0, 0x1D76);
                            SetComponentID(1, 0x1D75);
                            SetComponentID(2, 0x1D74);
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

        public CoffinAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((bool)m_Ankh);
            writer.Write((bool)m_East);
            writer.Write((bool)m_Opened);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Ankh = reader.ReadBool();
            m_East = reader.ReadBool();
            m_Opened = reader.ReadBool();
        }
    }

    public class CoffinAddonDeed : BaseChoiceAddonDeed
    {
        public override string DefaultName { get { return "a coffin deed"; } }
        public override BaseAddon Addon { get { return new CoffinAddon(m_Type + (m_Ankh ? 2 : 0)); } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Coffin (East)",
                "Coffin (South)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        private bool m_Ankh;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Ankh
        {
            get { return m_Ankh; }
            set { m_Ankh = value; }
        }

        [Constructable]
        public CoffinAddonDeed()
            : this(Utility.RandomBool())
        {
        }

        [Constructable]
        public CoffinAddonDeed(bool ankh)
            : base()
        {
            m_Ankh = ankh;
        }

        public CoffinAddonDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((bool)m_Ankh);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Ankh = reader.ReadBool();
        }
    }
}