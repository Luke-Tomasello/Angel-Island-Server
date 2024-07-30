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

/* Scripts/Items/Addons/AltarAddon.cs
 * ChangeLog
 *	11/2/23, Yoar
 *		Initial version
 */

namespace Server.Items
{
    public enum AltarType : byte
    {
        Compassion,
        Honesty,
        Honor,
        Humility,
        Justice,
        Sacrifice,
        Spirituality,
        Valor,

        Chaos,

        Mask = 0x0F,
        Flip = 0x10,
    }

    public class AltarAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new AltarAddonDeed(m_AltarType); } }
        public override bool Redeedable { get { return true; } }

        private AltarType m_AltarType;

        [CommandProperty(AccessLevel.GameMaster)]
        public AltarType AltarType
        {
            get { return m_AltarType; }
            set { m_AltarType = value; }
        }

        [Constructable]
        public AltarAddon()
            : this((AltarType)Utility.Random(8) | AltarType.Flip)
        {
        }

        [Constructable]
        public AltarAddon(AltarType altarType)
            : base()
        {
            m_AltarType = altarType & AltarType.Mask;

            int[] itemIDs = GetItemIDs(altarType & AltarType.Mask);

            if (itemIDs.Length == 4 || itemIDs.Length == 8)
            {
                int offset = ((itemIDs.Length == 8 && altarType.HasFlag(AltarType.Flip)) ? 4 : 0);

                AddComponent(new AddonComponent(itemIDs[0 + offset]), 0, 0, 0);
                AddComponent(new AddonComponent(itemIDs[1 + offset]), 0, 1, 0);
                AddComponent(new AddonComponent(itemIDs[2 + offset]), 1, 1, 0);
                AddComponent(new AddonComponent(itemIDs[3 + offset]), 1, 0, 0);
            }
            else
            {
                AddComponent(new AddonComponent(0x0), 0, 0, 0);
            }
        }

        private static readonly int[][] m_AltarIDs = new int[][]
            {
                new int[] { 0x14A7, 0x14A8, 0x14A9, 0x14AA, 0x14AB, 0x14AC, 0x14AD, 0x14AE }, // compassion
                new int[] { 0x149F, 0x14A0, 0x14A1, 0x14A2, 0x14A3, 0x14A4, 0x14A5, 0x14A6 }, // honesty
				new int[] { 0x14C7, 0x14C8, 0x14C9, 0x14CA, 0x14CB, 0x14CC, 0x14CD, 0x14CE }, // honor
				new int[] { 0x14CF, 0x14D0, 0x14D1, 0x14D2, 0x14D3, 0x14D4, 0x14D5, 0x14D6 }, // humility
				new int[] { 0x14AF, 0x14B0, 0x14B1, 0x14B2, 0x14B3, 0x14B4, 0x14B5, 0x14B6 }, // justice
				new int[] { 0x150A, 0x150B, 0x150C, 0x150D, 0x150E, 0x150F, 0x1510, 0x1511 }, // sacrifice
				new int[] { 0x14BF, 0x14C0, 0x14C1, 0x14C2, 0x14C3, 0x14C4, 0x14C5, 0x14C6 }, // spirituality
				new int[] { 0x14B7, 0x14B8, 0x14B9, 0x14BA, 0x14BB, 0x14BC, 0x14BD, 0x14BE }, // valor

				new int[] { 0x14E3, 0x14E4, 0x14E5, 0x14E6}, // chaos
            };

        private static int[] GetItemIDs(AltarType altarType)
        {
            int index = (int)altarType;

            if (index >= 0 && index < m_AltarIDs.Length)
                return m_AltarIDs[index];

            return new int[0];
        }

        public AltarAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((byte)m_AltarType);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_AltarType = (AltarType)reader.ReadByte();
        }
    }

    public class AltarAddonDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                AltarType altarType = m_AltarType;

                if (m_Type == 1)
                    altarType |= AltarType.Flip;

                return new AltarAddon(altarType);
            }
        }

        public override string DefaultName { get { return "an altar deed"; } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Shrine (East)",
                "Shrine (South)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        private AltarType m_AltarType;

        [CommandProperty(AccessLevel.GameMaster)]
        public AltarType AltarType
        {
            get { return m_AltarType; }
            set { m_AltarType = value; }
        }

        [Constructable]
        public AltarAddonDeed()
            : this((AltarType)Utility.Random(8))
        {
        }

        [Constructable]
        public AltarAddonDeed(AltarType altarType)
            : base()
        {
            m_AltarType = altarType & AltarType.Mask;
        }

        public AltarAddonDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((byte)m_AltarType);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_AltarType = (AltarType)reader.ReadByte();
        }
    }
}