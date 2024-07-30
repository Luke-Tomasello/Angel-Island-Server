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

/* Scripts/Items/TreasureThemes/DecorativeBow.cs
 * CHANGELOG
 *	04/01/05, Kitaras	
 *		Initial Creation
 */

namespace Server.Items
{

    public class DecorativeBow : Item
    {

        //4 differnt types 0-3
        [Constructable]
        public DecorativeBow(int type)
        {

            Name = "a decorative bow";
            Movable = true;

            if (type == 0) ItemID = 5468;
            if (type == 1) ItemID = 5469;
            if (type == 2) ItemID = 5470;
            if (type == 3) ItemID = 5471;
        }

        public DecorativeBow(Serial serial)
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