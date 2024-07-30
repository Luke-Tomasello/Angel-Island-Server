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

/* Items/Construction/GenericDoors/Door.cs
 * CHANGELOG:
 * 3/23/22, Yoar
 *	    Initial version.
 */

using Server.Spells.Third;

namespace Server.Items
{
    public enum DoorType : sbyte
    {
        Invalid = -1,

        MetalDoor,
        BarredMetalDoor,
        RattanDoor,
        DarkWoodDoor,
        MediumWoodDoor,
        MetalDoor2,
        LightWoodDoor,
        StrongWoodDoor,

        IronGate,
        LightWoodGate,
        IronGateShort,
        DarkWoodGate,

        BarredMetalDoor2,
    }

    /// <summary>
    /// Generic door type.
    /// </summary>
    public class Door : BaseDoor
    {
        [Constructable]
        public Door(DoorType doorType, DoorFacing facing)
            : this(DoorHelper.GetInfo(doorType), facing)
        {
        }

        private Door(DoorHelper.DoorInfo info, DoorFacing facing)
            : this(info.BaseItemID, info.BaseItemID + 1, info.BaseSoundID, info.BaseSoundID + 7, facing)
        {
        }

        [Constructable]
        public Door(int closedID, int openedID, int openedSound, int closedSound, DoorFacing facing)
            : base(closedID, openedID, -1, -1, openedSound, closedSound, facing, Point3D.Zero)
        {
        }

        public Door(Serial serial)
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

    /// <summary>
    /// Generic lockpickable door type.
    /// </summary>
    public class LockpickableDoor : BaseDoor, ILockpickable, IMagicUnlockable
    {
        private int m_LockLevel;
        private int m_MaxLockLevel;
        private int m_RequiredSkill;
        private Mobile m_Picker; // not serialized

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxLockLevel
        {
            get { return m_MaxLockLevel; }
            set { m_MaxLockLevel = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int LockLevel
        {
            get { return m_LockLevel; }
            set { m_LockLevel = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RequiredSkill
        {
            get { return m_RequiredSkill; }
            set { m_RequiredSkill = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Picker
        {
            get { return m_Picker; }
            set { m_Picker = value; }
        }

        [Constructable]
        public LockpickableDoor(DoorType doorType, DoorFacing facing)
            : this(DoorHelper.GetInfo(doorType), facing)
        {
        }

        private LockpickableDoor(DoorHelper.DoorInfo info, DoorFacing facing)
            : this(info.BaseItemID, info.BaseItemID + 1, info.BaseSoundID, info.BaseSoundID + 7, facing)
        {
        }

        [Constructable]
        public LockpickableDoor(int closedID, int openedID, int openedSound, int closedSound, DoorFacing facing)
            : base(closedID, openedID, -1, -1, openedSound, closedSound, facing, Point3D.Zero)
        {
        }

        public void LockPick(Mobile from)
        {
            Locked = false;
            Picker = from;

            if (Link != null)
            {
                Link.Locked = false;

                if (Link is ILockpickable)
                    ((ILockpickable)Link).Picker = from;
            }
        }

        public void OnMagicUnlock(Mobile from)
        {
            if (Link != null)
                Link.Locked = false;
        }

        public LockpickableDoor(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_LockLevel);
            writer.Write((int)m_MaxLockLevel);
            writer.Write((int)m_RequiredSkill);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_LockLevel = reader.ReadInt();
            m_MaxLockLevel = reader.ReadInt();
            m_RequiredSkill = reader.ReadInt();
        }
    }

    public static class DoorHelper
    {
        private static readonly DoorInfo[] m_Table = new DoorInfo[]
            {
                new DoorInfo( 0x675, 0xEC ), // metal door
				new DoorInfo( 0x685, 0xEC ), // barred metal door
				new DoorInfo( 0x695, 0xEB ), // rattan door
				new DoorInfo( 0x6A5, 0xEA ), // dark wood door
				new DoorInfo( 0x6B5, 0xEA ), // medium wood door
				new DoorInfo( 0x6C5, 0xEC ), // metal door 2
				new DoorInfo( 0x6D5, 0xEA ), // light wood door
				new DoorInfo( 0x6E5, 0xEA ), // strong wood door
				
				new DoorInfo( 0x824, 0xEC ), // iron gate
				new DoorInfo( 0x839, 0xEB ), // light wood gate
				new DoorInfo( 0x84C, 0xEC ), // iron gate short
				new DoorInfo( 0x866, 0xEB ), // dark wood gate

				new DoorInfo( 0x1FED, 0xEC ), // barred metal door 2
            };

        public static DoorInfo GetInfo(DoorType doorType)
        {
            int index = (int)doorType;

            if (index >= 0 && index < m_Table.Length)
                return m_Table[index];

            return DoorInfo.Invalid;
        }

        public static DoorType Identify(BaseDoor door)
        {
            int baseItemID = door.ClosedID;

            for (int i = 0; i < m_Table.Length; i++)
            {
                if (baseItemID >= m_Table[i].BaseItemID && baseItemID < m_Table[i].BaseItemID + 16)
                    return (DoorType)i;
            }

            return DoorType.Invalid;
        }

        public struct DoorInfo
        {
            public static readonly DoorInfo Invalid = new DoorInfo(0, -1);

            private int m_BaseItemID;
            private int m_BaseSoundID;

            public int BaseItemID { get { return m_BaseItemID; } }
            public int BaseSoundID { get { return m_BaseSoundID; } }

            public DoorInfo(int baseItemID, int baseSoundID)
            {
                m_BaseItemID = baseItemID;
                m_BaseSoundID = baseSoundID;
            }
        }
    }
}