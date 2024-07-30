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

/* Scripts/Items/Construction/Doors/SecretDoors.cs
 * CHANGELOG
 *	
 *	9/01/06 Taran Kain
 *		Changed constructors to fit new BaseDoor constructor
 */

namespace Server.Items
{
    public class SecretStoneDoor1 : BaseDoor
    {
        [Constructable]
        public SecretStoneDoor1(DoorFacing facing)
            : base(0xE8, 0xE9, 0xED, 0xF4, facing)
        {
        }

        public SecretStoneDoor1(Serial serial)
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

    public class SecretDungeonDoor : BaseDoor
    {
        [Constructable]
        public SecretDungeonDoor(DoorFacing facing)
            : base(0x314, 0x315, 0xED, 0xF4, facing)
        {
        }

        public SecretDungeonDoor(Serial serial)
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

    public class SecretStoneDoor2 : BaseDoor
    {
        [Constructable]
        public SecretStoneDoor2(DoorFacing facing)
            : base(0x324, 0x325, 0xED, 0xF4, facing)
        {
        }

        public SecretStoneDoor2(Serial serial)
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

    public class SecretWoodenDoor : BaseDoor
    {
        [Constructable]
        public SecretWoodenDoor(DoorFacing facing)
            : base(0x334, 0x335, 0xED, 0xF4, facing)
        {
        }

        public SecretWoodenDoor(Serial serial)
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

    public class SecretLightWoodDoor : BaseDoor
    {
        [Constructable]
        public SecretLightWoodDoor(DoorFacing facing)
            : base(0x344, 0x345, 0xED, 0xF4, facing)
        {
        }

        public SecretLightWoodDoor(Serial serial)
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

    public class SecretStoneDoor3 : BaseDoor
    {
        [Constructable]
        public SecretStoneDoor3(DoorFacing facing)
            : base(0x354, 0x355, 0xED, 0xF4, facing)
        {
        }

        public SecretStoneDoor3(Serial serial)
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