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

/* Scripts/Items/Addons/ChristmasTreeAddon.cs
 * ChangeLog
 *	12/27/23, Yoar
 *		Initial version
 */

using System;

namespace Server.Items
{
    public class ChristmasTreeAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new ChristmasTreeAddonDeed(); } }

        [Constructable]
        public ChristmasTreeAddon()
            : this(0)
        {
        }

        [Constructable]
        public ChristmasTreeAddon(int type)
        {
            switch (type)
            {
                case 0: // Classic
                    {
                        AddComponent(new TreeComponent(0xCD6), 0, 0, 0);
                        AddComponent(new TreeComponent(0xCD7), 0, 0, 0);

                        AddOrnaments(m_ClassicOrnaments);

                        break;
                    }
                case 1: // Modern
                    {
                        AddComponent(new TreeComponent(0x1B7E), 0, 0, 0);

                        AddOrnaments(m_ModernOrnaments);

                        break;
                    }
            }
        }

        private void AddOrnaments(OrnamentEntry[] ornaments)
        {
            for (int i = 0; i < ornaments.Length; i++)
                AddComponent(new OrnamentComponent(ornaments[i].ItemID), ornaments[i].X, ornaments[i].Y, ornaments[i].Z);
        }

        public ChristmasTreeAddon(Serial serial)
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

        private static readonly OrnamentEntry[] m_ClassicOrnaments = new OrnamentEntry[]
            {
                new OrnamentEntry(0, 0,  2, 0xF22),
                new OrnamentEntry(0, 0,  9, 0xF18),
                new OrnamentEntry(0, 0, 15, 0xF20),
                new OrnamentEntry(0, 0, 19, 0xF17),
                new OrnamentEntry(0, 0, 20, 0xF24),
                new OrnamentEntry(0, 0, 20, 0xF1F),
                new OrnamentEntry(0, 0, 20, 0xF19),
                new OrnamentEntry(0, 0, 21, 0xF1B),
                new OrnamentEntry(0, 0, 28, 0xF2F),
                new OrnamentEntry(0, 0, 30, 0xF23),
                new OrnamentEntry(0, 0, 32, 0xF2A),
                new OrnamentEntry(0, 0, 33, 0xF30),
                new OrnamentEntry(0, 0, 34, 0xF29),
                new OrnamentEntry(0, 1,  7, 0xF16),
                new OrnamentEntry(0, 1,  7, 0xF1E),
                new OrnamentEntry(0, 1, 12, 0xF0F),
                new OrnamentEntry(0, 1, 13, 0xF13),
                new OrnamentEntry(0, 1, 18, 0xF12),
                new OrnamentEntry(0, 1, 19, 0xF15),
                new OrnamentEntry(0, 1, 25, 0xF28),
                new OrnamentEntry(0, 1, 29, 0xF1A),
                new OrnamentEntry(0, 1, 37, 0xF2B),
                new OrnamentEntry(1, 0, 13, 0xF10),
                new OrnamentEntry(1, 0, 14, 0xF1C),
                new OrnamentEntry(1, 0, 16, 0xF14),
                new OrnamentEntry(1, 0, 17, 0xF26),
                new OrnamentEntry(1, 0, 22, 0xF27),
            };

        private static readonly OrnamentEntry[] m_ModernOrnaments = new OrnamentEntry[]
            {
                new OrnamentEntry(0, 0,  2, 0xF2F),
                new OrnamentEntry(0, 0,  2, 0xF20),
                new OrnamentEntry(0, 0,  2, 0xF22),
                new OrnamentEntry(0, 0,  5, 0xF30),
                new OrnamentEntry(0, 0,  5, 0xF15),
                new OrnamentEntry(0, 0,  5, 0xF1F),
                new OrnamentEntry(0, 0,  5, 0xF2B),
                new OrnamentEntry(0, 0,  6, 0xF0F),
                new OrnamentEntry(0, 0,  7, 0xF1E),
                new OrnamentEntry(0, 0,  7, 0xF24),
                new OrnamentEntry(0, 0,  8, 0xF29),
                new OrnamentEntry(0, 0,  9, 0xF18),
                new OrnamentEntry(0, 0, 14, 0xF1C),
                new OrnamentEntry(0, 0, 15, 0xF13),
                new OrnamentEntry(0, 0, 15, 0xF20),
                new OrnamentEntry(0, 0, 16, 0xF26),
                new OrnamentEntry(0, 0, 17, 0xF12),
                new OrnamentEntry(0, 0, 18, 0xF17),
                new OrnamentEntry(0, 0, 20, 0xF1B),
                new OrnamentEntry(0, 0, 23, 0xF28),
                new OrnamentEntry(0, 0, 25, 0xF18),
                new OrnamentEntry(0, 0, 25, 0xF2A),
                new OrnamentEntry(0, 1,  7, 0xF16),
            };

        private class OrnamentEntry
        {
            private int m_X, m_Y, m_Z;
            private int m_ItemID;

            public int X { get { return m_X; } }
            public int Y { get { return m_Y; } }
            public int Z { get { return m_Z; } }
            public int ItemID { get { return m_ItemID; } }

            public OrnamentEntry(int x, int y, int z, int itemID)
            {
                m_X = x;
                m_Y = y;
                m_Z = z;
                m_ItemID = itemID;
            }
        }

        private class TreeComponent : AddonComponent
        {
            public override string DefaultName { get { return "a Christmas tree"; } }

            public TreeComponent(int itemID)
                : base(itemID)
            {
            }

            public TreeComponent(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.WriteEncodedInt(0);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                reader.ReadEncodedInt();
            }
        }

        private class OrnamentComponent : AddonComponent
        {
            public override string DefaultName { get { return "a Christmas tree ornament"; } }

            public OrnamentComponent(int itemID)
                : base(itemID)
            {
            }

            public OrnamentComponent(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.WriteEncodedInt(0);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                reader.ReadEncodedInt();
            }
        }
    }

    public class ChristmasTreeAddonDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon { get { return new ChristmasTreeAddon(m_Type); } }
        public override int LabelNumber { get { return 1041117; } } // a tree for the holidays

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                1018322, // Classic
                1018321, // Modern
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public ChristmasTreeAddonDeed()
        {
            Hue = 0x47; // old-style holiday deeds are hued
        }

        public override bool CanPlace(Mobile from)
        {
            if (DateTime.UtcNow.Month != 12)
            {
                from.SendLocalizedMessage(1005700); // You will have to wait till next December to put your tree back up for display.
                return false;
            }

            return true;
        }

        public ChristmasTreeAddonDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (version < 1 && Hue == 0)
                Hue = 0x47; // old-style holiday deeds are hued
        }
    }
}