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

/* scripts\Items\misc\NoDropTarget.cs
 *	ChangeLog :
 *	5/19/2023, Adam
 *		Created. Disallow items to be dropped at this location
 *		Checked in Item.DropToWorld()
 */

namespace Server.Items
{
    public class NoDropTarget : Item
    {
        [Constructable]
        public NoDropTarget()
            : base(0x1B7A)
        {
            Movable = false;
            Visible = false;
            Name = "no drop target";
        }

        public NoDropTarget(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}