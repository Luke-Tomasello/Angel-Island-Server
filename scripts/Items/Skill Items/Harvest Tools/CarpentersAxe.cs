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

/* Scripts\Items\Skill Items\Harvest Tools\CarpentersAxe.cs
 * CHANGELOG:
 *  12/26/23, Yoar:
 *      Changed the hue of carpenter's axes to better distinguish them from normal hatchets.
 *	11/28/23, Yoar: 
 *	    Initial commit.
 *	    Instantly processes any chopped logs into boards.
 */

namespace Server.Items
{
    public class CarpentersAxe : Hatchet
    {
        public override string DefaultName { get { return "carpenter's axe"; } }

        [Constructable]
        public CarpentersAxe()
            : base()
        {
            Hue = 0x979;
        }

        public CarpentersAxe(Serial serial)
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
                Hue = 0x979;
        }
    }
}