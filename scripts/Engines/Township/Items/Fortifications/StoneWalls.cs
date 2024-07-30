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

/* Engines/Township/Items/Fortifications/StoneWalls.cs
 * CHANGELOG:
 * 11/20/21, Yoar
 *	    Refactored township walls/tools.
 */

namespace Server.Township
{
    public class StoneFortificationWall : FortificationWall
    {
        [Constructable]
        public StoneFortificationWall()
            : this(0x001C)
        {
        }

        [Constructable]
        public StoneFortificationWall(int itemID)
            : base(itemID)
        {
            Weight = 200;
        }

        protected override int GetInitialHits(Mobile m)
        {
            return 2 * base.GetInitialHits(m);
        }

        public StoneFortificationWall(Serial serial)
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