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

/* Scripts\Engines\AngelIsland\Items\GuardPostDoor.cs
 * ChangeLog:
 *  5/29/2021, Adam
 */

namespace Server.Items
{
    [Flipable]
    public class GuardPostDoor : BaseDoor
    {
        [Constructable]
        public GuardPostDoor(DoorFacing facing)
            : base(0x322 - (2 * (int)facing), 0x323 - (2 * (int)facing), -1, -1, 0xED, 0xF4, facing, Point3D.Zero)
        {
        }

        public GuardPostDoor(Serial serial)
            : base(serial)
        {
        }
        public override void FailUnlockMessage(Mobile from, int number)
        {
            // number = 501668; // This key doesn't seem to unlock that.
            // number = 501666; // You can't unlock that!
            from.SendMessage("Perhaps that last guard changed the locks?");
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