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

/* Scripts/Items/Special/Holiday/Halloween/CasketAddon.cs
 * CHANGELOG
 *  10/5/23, Yoar
 *      Initial commit.
 */

namespace Server.Items
{
    public class CasketAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new CasketAddonDeed(); } }
        public override bool Redeedable { get { return true; } }

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
        public CasketAddon()
            : this(1)
        {
        }

        [Constructable]
        public CasketAddon(int type)
        {
            m_East = (type == 0);

            if (m_East)
            {
                AddComponent(new AddonComponent(0x1C25), -1, 0, 0);
                AddComponent(new AddonComponent(0x1C26), 0, 0, 0);
                AddComponent(new AddonComponent(0x1C27), 1, 0, 0);
                AddComponent(new AddonComponent(0x1C24), -1, 1, 0);
                AddComponent(new AddonComponent(0x1C23), 0, 1, 0);
                AddComponent(new AddonComponent(0x1C22), 1, 1, 0);

                AddComponent(new AddonComponent(0x1), -1, 0, 0);
            }
            else
            {
                AddComponent(new AddonComponent(0x1C32), 0, -1, 0);
                AddComponent(new AddonComponent(0x1C33), 0, 0, 0);
                AddComponent(new AddonComponent(0x1C34), 0, 1, 0);
                AddComponent(new AddonComponent(0x1C31), 1, -1, 0);
                AddComponent(new AddonComponent(0x1C30), 1, 0, 0);
                AddComponent(new AddonComponent(0x1C2F), 1, 1, 0);

                AddComponent(new AddonComponent(0x1), 0, -1, 0);
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
#if false
            if ( Map == null )
                return true;

            Point3D offset = ( m_East ? new Point3D( -2, 0, 0 ) : new Point3D( 0, -2, 0 ) );

            return Map.CanFit( Location + offset, 16, false, true, false );
#else
            return true;
#endif
        }

        private void Open()
        {
            m_Opened = true;

            Point3D offset = (m_East ? new Point3D(-1, 0, 0) : new Point3D(0, -1, 0));

            MoveComponent(6, offset);

            int type = Randomize();

            switch (type)
            {
                case 0: // empty
                    {
                        if (m_East)
                        {
                            SetComponentID(0, 0x1C2C);
                            SetComponentID(1, 0x1C2D);
                            SetComponentID(2, 0x1C2E);
                            SetComponentID(3, 0x1C2A);
                            SetComponentID(4, 0x1C29);
                            SetComponentID(5, 0x1C28);

                            SetComponentID(6, 0x1C2B);
                        }
                        else
                        {
                            SetComponentID(0, 0x1C39);
#if false
                        SetComponentID( 1, 0x1C3A );
#else
                            SetComponentID(1, 0x1D34);
#endif
                            SetComponentID(2, 0x1C3B);
                            SetComponentID(3, 0x1C37);
                            SetComponentID(4, 0x1C36);
                            SetComponentID(5, 0x1C35);

                            SetComponentID(6, 0x1C38);
                        }

                        break;
                    }
                case 1: // body
                    {
                        if (m_East)
                        {
                            SetComponentID(0, 0x1D16);
                            SetComponentID(1, 0x1D17);
                            SetComponentID(2, 0x1C2E);
                            SetComponentID(3, 0x1D15);
                            SetComponentID(4, 0x1D14);
                            SetComponentID(5, 0x1D13);

                            SetComponentID(6, 0x1C2B);
                        }
                        else
                        {
                            SetComponentID(0, 0x1D33);
                            SetComponentID(1, 0x1D34);
                            SetComponentID(2, 0x1C3B);
                            SetComponentID(3, 0x1D32);
                            SetComponentID(4, 0x1D31);
                            SetComponentID(5, 0x1D30);

                            SetComponentID(6, 0x1C38);
                        }

                        break;
                    }
                case 2: // skeleton
                    {
                        if (m_East)
                        {
                            SetComponentID(0, 0x1D4F);
#if false
                        SetComponentID( 1, 0x1D50 );
#else
                            SetComponentID(1, 0x1C2D);
#endif
                            SetComponentID(2, 0x1D51);
                            SetComponentID(3, 0x1D4E);
                            SetComponentID(4, 0x1D4D);
                            SetComponentID(5, 0x1D4C);

                            SetComponentID(6, 0x1C2B);
                        }
                        else
                        {
                            SetComponentID(0, 0x1D6F);
                            SetComponentID(1, 0x1D70);
                            SetComponentID(2, 0x1D71);
                            SetComponentID(3, 0x1D6E);
                            SetComponentID(4, 0x1D6D);
                            SetComponentID(5, 0x1D6C);

                            SetComponentID(6, 0x1C38);
                        }

                        break;
                    }
            }

            Effects.PlaySound(Location, Map, 0xEB);

            if (type != 0)
                Effects.PlaySound(Location, Map, Utility.RandomBool() ? 0x3EA : 0x3EC);
        }

        private void Close()
        {
            m_Opened = false;

            Point3D offset = (m_East ? new Point3D(1, 0, 0) : new Point3D(0, 1, 0));

            MoveComponent(6, offset);

            if (m_East)
            {
                SetComponentID(0, 0x1C25);
                SetComponentID(1, 0x1C26);
                SetComponentID(2, 0x1C27);
                SetComponentID(3, 0x1C24);
                SetComponentID(4, 0x1C23);
                SetComponentID(5, 0x1C22);

                SetComponentID(6, 0x1);
            }
            else
            {
                SetComponentID(0, 0x1C32);
                SetComponentID(1, 0x1C33);
                SetComponentID(2, 0x1C34);
                SetComponentID(3, 0x1C31);
                SetComponentID(4, 0x1C30);
                SetComponentID(5, 0x1C2F);

                SetComponentID(6, 0x1);
            }

            Effects.PlaySound(Location, Map, 0xF2);
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

        public CasketAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((bool)m_East);
            writer.Write((bool)m_Opened);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_East = reader.ReadBool();
            m_Opened = reader.ReadBool();
        }
    }

    public class CasketAddonDeed : BaseChoiceAddonDeed
    {
        public override string DefaultName { get { return "a burial casket deed"; } }
        public override BaseAddon Addon { get { return new CasketAddon(m_Type); } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Casket (East)",
                "Casket (South)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public CasketAddonDeed()
            : base()
        {
        }

        public CasketAddonDeed(Serial serial)
            : base(serial)
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