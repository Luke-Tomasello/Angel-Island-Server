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

/* Scripts/Items/Construction/Doors/Doors.cs
 * CHANGELOG
 *	9/03/06 Taran Kain
 *		Moved DoorFacing enum definition to BaseDoor.
 *	9/01/06 Taran Kain
 *		Modified constructors to fit new BaseDoor constructor
 */

namespace Server.Items
{
    public class IronGateShort : BaseDoor
    {
        [Constructable]
        public IronGateShort(DoorFacing facing)
            : base(0x84c, 0x84d, 0xEC, 0xF3, facing)
        {
        }

        public IronGateShort(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class IronGate : BaseDoor
    {
        [Constructable]
        public IronGate(DoorFacing facing)
            : base(0x824, 0x825, 0xEC, 0xF3, facing)
        {
        }

        public IronGate(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class LightWoodGate : BaseDoor
    {
        [Constructable]
        public LightWoodGate(DoorFacing facing)
            : base(0x839, 0x83A, 0xEB, 0xF2, facing)
        {
        }

        public LightWoodGate(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class DarkWoodGate : BaseDoor
    {
        [Constructable]
        public DarkWoodGate(DoorFacing facing)
            : base(0x866, 0x867, 0xEB, 0xF2, facing)
        {
        }

        public DarkWoodGate(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class MetalDoor : BaseDoor
    {
        [Constructable]
        public MetalDoor(DoorFacing facing)
            : base(0x675, 0x676, 0xEC, 0xF3, facing)
        {
        }

        public MetalDoor(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class BarredMetalDoor : BaseDoor
    {
        [Constructable]
        public BarredMetalDoor(DoorFacing facing)
            : base(0x685, 0x686, 0xEC, 0xF3, facing)
        {
        }

        public BarredMetalDoor(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class BarredMetalDoor2 : BaseDoor
    {
        [Constructable]
        public BarredMetalDoor2(DoorFacing facing)
            : base(0x1FED, 0x1FEE, 0xEC, 0xF3, facing)
        {
        }

        public BarredMetalDoor2(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class RattanDoor : BaseDoor
    {
        [Constructable]
        public RattanDoor(DoorFacing facing)
            : base(0x695, 0x696, 0xEB, 0xF2, facing)
        {
        }

        public RattanDoor(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class DarkWoodDoor : BaseDoor
    {
        [Constructable]
        public DarkWoodDoor(DoorFacing facing)
            : base(0x6A5, 0x6A6, 0xEA, 0xF1, facing)
        {
        }

        public DarkWoodDoor(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class MediumWoodDoor : BaseDoor
    {
        [Constructable]
        public MediumWoodDoor(DoorFacing facing)
            : base(0x6B5, 0x6B6, 0xEA, 0xF1, facing)
        {
        }

        public MediumWoodDoor(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class MetalDoor2 : BaseDoor
    {
        [Constructable]
        public MetalDoor2(DoorFacing facing)
            : base(0x6C5, 0x6C6, 0xEC, 0xF3, facing)
        {
        }

        public MetalDoor2(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class LightWoodDoor : BaseDoor
    {
        [Constructable]
        public LightWoodDoor(DoorFacing facing)
            : base(0x6D5, 0x6D6, 0xEA, 0xF1, facing)
        {
        }

        public LightWoodDoor(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class StrongWoodDoor : BaseDoor
    {
        [Constructable]
        public StrongWoodDoor(DoorFacing facing)
            : base(0x6E5, 0x6E6, 0xEA, 0xF1, facing)
        {
        }

        public StrongWoodDoor(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer) // Default Serialize method
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader) // Default Deserialize method
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}