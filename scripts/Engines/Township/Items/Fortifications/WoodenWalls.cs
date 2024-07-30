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

/* Engines/Township/Items/Fortifications/WoodenWalls.cs
 * CHANGELOG:
 * 11/20/21, Yoar
 *	    Refactored township walls/tools.
 */

namespace Server.Township
{
    [TypeAlias("Server.Township.SpearFortificationWall")]
    public class WoodFortificationWall : FortificationWall
    {
        [Constructable]
        public WoodFortificationWall()
            : this(0x221)
        {
        }

        [Constructable]
        public WoodFortificationWall(int itemID)
            : base(itemID)
        {
        }

        public WoodFortificationWall(Serial serial)
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